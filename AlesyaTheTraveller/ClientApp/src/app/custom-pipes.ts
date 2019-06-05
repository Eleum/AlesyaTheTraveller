import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'transform' })
export class NumberToArrayPipe implements PipeTransform {
  transform(value, args: string[]): any {
    const res = [];
    for (let i = 0; i < value; i++) {
      res.push(i);
    }
    return res;
  }
}

@Pipe({ name: 'description' })
export class NumberToDescriptionPipe implements PipeTransform {
  transform(value, args: string[]): any {
    let ending = "";
    if (value % 10 === 1) {
      ending = "ка";
    } else if (value % 10 === 2 || value % 10 === 3) {
      ending = "ки";
    } else if (value >= 11 && value <= 19) {
      ending = "ок";
    } else {
      ending = "ок";
    }
    return `${value == 0 ? "нет" : value} пересад${ending}`;
  }
}
